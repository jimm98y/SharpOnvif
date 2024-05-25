using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Search
{
    [DisableMustUnderstandValidation]
    public class SearchPortBase : SearchPort
    {
        [return: MessageParameter(Name = "Endpoint")]
        public virtual DateTime EndSearch(string SearchToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SearchToken")]
        public virtual FindEventsResponse FindEvents(FindEventsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SearchToken")]
        public virtual FindMetadataResponse FindMetadata(FindMetadataRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SearchToken")]
        public virtual FindPTZPositionResponse FindPTZPosition(FindPTZPositionRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SearchToken")]
        public virtual FindRecordingsResponse FindRecordings(FindRecordingsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ResultList")]
        public virtual GetEventSearchResultsResponse GetEventSearchResults(GetEventSearchResultsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "MediaAttributes")]
        public virtual GetMediaAttributesResponse GetMediaAttributes(GetMediaAttributesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ResultList")]
        public virtual GetMetadataSearchResultsResponse GetMetadataSearchResults(GetMetadataSearchResultsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ResultList")]
        public virtual GetPTZPositionSearchResultsResponse GetPTZPositionSearchResults(GetPTZPositionSearchResultsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RecordingInformation")]
        public virtual RecordingInformation GetRecordingInformation(string RecordingToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ResultList")]
        public virtual GetRecordingSearchResultsResponse GetRecordingSearchResults(GetRecordingSearchResultsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Summary")]
        public virtual RecordingSummary GetRecordingSummary()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "State")]
        public virtual SearchState GetSearchState(string SearchToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }
    }
}
