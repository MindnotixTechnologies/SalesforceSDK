using System;
using Xamarin.Auth;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Salesforce
{
	public static class SalesforceClientExtensions
	{
		public static async Task<string> CreateAsync (this SalesforceClient self, SObject sobject)
		{
			var createRequest = new CreateRequest (sobject);
			Response result;
			try {
				result = await self.ProcessAsync (createRequest).ConfigureAwait (true);
			} catch (AggregateException ex) {
				Debug.WriteLine (ex.Message);
				return null;
			}
			var json = result.GetResponseText ();
			var jsonValue = JsonValue.Parse (json);
			if (jsonValue == null)
				return null;
			sobject.Id = jsonValue["id"];
			return sobject.Id;
		}

		public static string Create (this SalesforceClient self, SObject sobject)
		{
			var createRequest = new CreateRequest (sobject);
			var result = self.ProcessAsync (createRequest);
			if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout)))
				return null;

			if (result.IsFaulted)
				return null; // TODO: Do error reporting

			var json = result.Result.GetResponseText ();
			var jsonValue = JsonValue.Parse (json);
			if (jsonValue == null)
				return null;
			sobject.Id = jsonValue["id"];
			return sobject.Id;
		}

		public static async Task UpdateAsync (this SalesforceClient self, SObject sobject)
		{
			var updateRequest = new UpdateRequest (sobject);
			try {
				await self.ProcessAsync (updateRequest).ConfigureAwait (true);
			} catch (AggregateException ex) {
				Debug.WriteLine (ex.Message);
			}
		}

		public static void Update (this SalesforceClient self, SObject sobject)
		{
			var updateRequest = new UpdateRequest (sobject);
			var result = self.ProcessAsync (updateRequest);
			if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout)))
				return; // TODO : Error handling/reporting
		}

		public static IEnumerable<SearchResult> Search (this SalesforceClient self, string search)
		{
			var result = self.SearchAsync (search);
			if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout))) {
				Debug.WriteLine ("Request timed out");
				return null;
			}

			return result.Result;
		}

		public async static Task<IEnumerable<SearchResult>> SearchAsync (this SalesforceClient self, string search)
		{
			var request = new ReadRequest {Resource = new Search {QueryText = search}};

			Response response;

			try {
				response = await self.ProcessAsync (request);
			} catch (AggregateException ex) {
				throw ex.Flatten ().InnerException;
			}

			if (response == null) {
				return null;
			}

			var result = response.GetResponseText ();
			var jsonValue = (JsonArray)JsonValue.Parse (result);

			if (jsonValue == null)
				throw new Exception ("Could not parse Json data");

			var returnVal = jsonValue.Select(jv => new SearchResult(jv)).ToArray();
			return returnVal;

		}

		public static Task<IEnumerable<SObject>> QueryAsync (this SalesforceClient self, string query)
		{
			return self.ReadAsync (new ReadRequest {Resource = new Query {Statement = query}});
		}

		public static async Task<IEnumerable<SObject>> ReadAsync (this SalesforceClient self, ReadRequest request)
		{
			Response response;

			try {
				response = await self.ProcessAsync (request);
			} catch (AggregateException ex) {
				throw ex.Flatten ().InnerException;
			}

			if (response == null) {
				return Enumerable.Empty<SObject> ();
			}

			var result = response.GetResponseText ();
			var jsonValue = JsonValue.Parse (result);

			if (jsonValue == null)
				throw new Exception ("Could not parse Json data");

			var results = jsonValue["records"];
			return results.OfType<JsonObject> ().Select (j => new SObject (j));
		}

		public static IEnumerable<SObject> Read (this SalesforceClient self, ReadRequest request)
		{
			var result = self.ReadAsync (request);
			if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout))) {
				Debug.WriteLine ("Request timed out");
				return Enumerable.Empty<SObject> ();
			}

			return result.Result;
		}

		public static IEnumerable<SObject> Query (this SalesforceClient self, string query)
		{
			var result = self.QueryAsync (query);
			if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout))) {
				Debug.WriteLine ("Request timed out");
				return Enumerable.Empty<SObject> ();
			}

			return result.Result;
		}

		public static async Task<bool> DeleteAsync (this SalesforceClient self, SObject sobject)
		{
			// Delete the row from the data source.
			var request = new DeleteRequest (sobject);
			var response = await self.ProcessAsync (request);
			return response.StatusCode == System.Net.HttpStatusCode.NoContent;
		}

		public static bool Delete (this SalesforceClient self, SObject sobject)
		{
			var result = self.DeleteAsync (sobject);
			if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout))) {
				Debug.WriteLine ("Request timed out");
				return false;
			}
			return result.Result;
		}

        public static JsonObject Describe (this SalesforceClient self, string type)
        {
            var result = self.DescribeAsync (type);
            if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout)))
                return null; // TODO : Error handling/reporting
            return result.Result;
        }

		public static async Task<JsonObject> DescribeAsync (this SalesforceClient self, string type)
		{
			var request = new ReadRequest { Resource = new SObject { Id = "describe", ResourceName = type } };

			Response response;

			try {
				response = await self.ProcessAsync (request);
			} catch (AggregateException ex) {
				throw ex.Flatten ().InnerException;
			}

			var result = response.GetResponseText ();
			var jsonValue = JsonValue.Parse (result);

			if (jsonValue == null)
				throw new Exception ("Could not parse Json data");

			return (JsonObject)jsonValue;
		}

        public static JsonObject Changes (this SalesforceClient self, string type, ChangeTypes kind, DateTime from, DateTime until)
		{
            var request = new ChangesRequest {
                Resource = new SObject { Id = kind.ToString().ToLowerInvariant(), ResourceName = type},
                Since = from,
                Until = until
            };

            var result = self.ChangesAsync (request);
			if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout)))
				return null; // TODO : Error handling/reporting
			return result.Result;
		}

        public static JsonObject Changes (this SalesforceClient self, string type, ChangeTypes kind, DateTime from)
        {
            var request = new ChangesRequest {
                Resource = new SObject { Id = kind.ToString().ToLowerInvariant(), ResourceName = type},
                Since = from
            };

            var result = self.ChangesAsync (request);
            if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout)))
                return null; // TODO : Error handling/reporting
            return result.Result;
        }

        public static JsonObject Changes (this SalesforceClient self, string type, ChangeTypes kind)
        {
            var request = new ChangesRequest {
                Resource = new SObject { Id = kind.ToString().ToLowerInvariant(), ResourceName = type}
            };

            var result = self.ChangesAsync (request);
            if (!result.Wait (TimeSpan.FromSeconds (SalesforceClient.DefaultNetworkTimeout)))
                return null; // TODO : Error handling/reporting
            return result.Result;
        }

        public static async Task<JsonObject> ChangesAsync (this SalesforceClient self, string type, ChangeTypes kind, DateTime from, DateTime until)
        {
            var request = new ChangesRequest {
                Resource = new SObject { Id = kind.ToString().ToLowerInvariant(), ResourceName = type},
                Since = from,
                Until = until
            };

            return await self.ChangesAsync (request);
        }

        public static async Task<JsonObject> ChangesAsync (this SalesforceClient self, string type, ChangeTypes kind, DateTime from)
        {
            var request = new ChangesRequest {
                Resource = new SObject { Id = kind.ToString().ToLowerInvariant(), ResourceName = type},
                Since = from
            };

            return await self.ChangesAsync (request);
        }

        public static async Task<JsonObject> ChangesAsync (this SalesforceClient self, string type, ChangeTypes kind)
        {
            var request = new ChangesRequest {
                Resource = new SObject { Id = kind.ToString().ToLowerInvariant(), ResourceName = type},
            };

            return await self.ChangesAsync (request);
        }

        /// <summary>
        /// Requests changes to all SObjects of the specified type in the given time frame.
        /// </summary>
        /// <remarks>
        /// Returns updates if passed ChangeTypes.Default.
        /// 
        /// Start date must not exceed 30 days prior.
        /// 
        /// Salesforce limits the result set size to 200,000 id's.
        /// If your request matches more than that, the service will
        /// return an EXCEEDED_ID_LIMIT error. To resolve this, either narrow
        /// down your time interval or sub-divide the internal into
        /// multiple requests.
        /// </remarks>
        /// <returns>The async.</returns>
        /// <param name="self">Self.</param>
        /// <param name = "request"></param>
        public static async Task<JsonObject> ChangesAsync (this SalesforceClient self, IAuthenticatedRequest request)
        {
            Response response;

            try {
                response = await self.ProcessAsync (request);
            } catch (AggregateException ex) {
                throw ex.Flatten ().InnerException;
            }

            var result = response.GetResponseText ();
            var jsonValue = JsonValue.Parse (result);

            if (jsonValue == null)
                throw new Exception ("Could not parse Json data");

            return (JsonObject)jsonValue;
        }
	}
}
