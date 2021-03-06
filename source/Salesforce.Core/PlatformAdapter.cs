using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Auth;

#if PLATFORM_ANDROID
using Android.Content;
#endif

namespace Salesforce
{
    /// <summary>
    /// Prevents platform abstractions from leaking.
    /// </summary>
    public interface IPlatformAdapter
    {
        Authenticator Authenticator { get; set; }
        object GetLoginUI();
        void SaveAccount(ISalesforceUser account);
        IEnumerable<ISalesforceUser> LoadAccounts();
    }

    public struct PlatformStrings
    {
        static String salesforce = "Salesforce";

        /// <summary>
        /// Identifies our credentials in the credential store.
        /// </summary>
        /// <value>The salesforce.</value>
        /// <remarks>
        /// If you are going to change this value,
        /// be sure to do it before constructing
        /// a new <see cref="SalesforceClient"/>.
        /// </remarks>
        public static String CredentialStoreServiceName
        {
            get { return salesforce; }
            set { salesforce = value; }
        }
    }

#if PLATFORM_ANDROID
	internal class AndroidPlatformAdapter : IPlatformAdapter
	{
		static AndroidPlatformAdapter()
		{
			CurrentPlatformContext = global::Android.App.Application.Context;
		}

		public Authenticator Authenticator { get; set;	}

		public static Context CurrentPlatformContext { get; set; }

    #region IPlatformAdapter implementation

		public object GetLoginUI()
		{
			return Authenticator.GetUI (CurrentPlatformContext as Context);
		}

		public void SaveAccount (ISalesforceUser account)
		{
			AccountStore
				.Create (CurrentPlatformContext as Context ?? global::Android.App.Application.Context)
                .Save ((Xamarin.Auth.Account) account, PlatformStrings.CredentialStoreServiceName);
		}

    #endregion

		public AndroidPlatformAdapter ()
		{
		}

		public AndroidPlatformAdapter (Authenticator activator)
		{
			this.Authenticator = activator;
		}

		public IEnumerable<ISalesforceUser> LoadAccounts()
		{
			return AccountStore
                    .Create (CurrentPlatformContext as Context)
                    .FindAccountsForService (PlatformStrings.CredentialStoreServiceName)
                    .Cast<ISalesforceUser>()        // using System.Linq
                    ;
		}

}
#endif

#if PLATFORM_IOS
	internal class UIKitPlatformAdapter : IPlatformAdapter
	{
		public  Authenticator Authenticator { get; set;	}

    #region IPlatformAdapter implementation

		public UIKitPlatformAdapter() : this(null)
		{ }

		public UIKitPlatformAdapter(Authenticator activator)
		{
			this.Authenticator = activator;
		}

		public object GetLoginUI ()
		{
			return Authenticator.GetUI ();
		}

		public void SaveAccount (ISalesforceUser account)
		{
            AccountStore.Create ().Save ((Xamarin.Auth.Account)account, PlatformStrings.CredentialStoreServiceName);
		}

		public IEnumerable<ISalesforceUser> LoadAccounts()
		{
            return AccountStore
                    .Create ()
                    .FindAccountsForService (PlatformStrings.CredentialStoreServiceName)
                    .Cast<ISalesforceUser>()        // using System.Linq
                    ;
		}

    #endregion

	}
#endif
}

