using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Recording
{
    [DisableMustUnderstandValidation]
    public class RecordingBase : RecordingPort
    {
        [return: MessageParameter(Name = "RecordingToken")]
        public virtual string CreateRecording(RecordingConfiguration RecordingConfiguration)
        {
            throw new NotImplementedException();
        }

        public virtual CreateRecordingJobResponse CreateRecordingJob(CreateRecordingJobRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "TrackToken")]
        public virtual string CreateTrack(string RecordingToken, TrackConfiguration TrackConfiguration)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteRecording(string RecordingToken)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteRecordingJob(string JobToken)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteTrack(string RecordingToken, string TrackToken)
        {
            throw new NotImplementedException();
        }

        public virtual ExportRecordedDataResponse ExportRecordedData(ExportRecordedDataRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetExportRecordedDataStateResponse GetExportRecordedDataState(GetExportRecordedDataStateRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RecordingConfiguration")]
        public virtual RecordingConfiguration GetRecordingConfiguration(string RecordingToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "JobConfiguration")]
        public virtual RecordingJobConfiguration GetRecordingJobConfiguration(string JobToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "JobItem")]
        public virtual GetRecordingJobsResponse GetRecordingJobs(GetRecordingJobsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "State")]
        public virtual RecordingJobStateInformation GetRecordingJobState(string JobToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual RecordingOptions GetRecordingOptions(string RecordingToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RecordingItem")]
        public virtual GetRecordingsResponse GetRecordings(GetRecordingsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "TrackConfiguration")]
        public virtual TrackConfiguration GetTrackConfiguration(string RecordingToken, string TrackToken)
        {
            throw new NotImplementedException();
        }

        public virtual void SetRecordingConfiguration(string RecordingToken, RecordingConfiguration RecordingConfiguration)
        {
            throw new NotImplementedException();
        }

        public virtual SetRecordingJobConfigurationResponse SetRecordingJobConfiguration(SetRecordingJobConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetRecordingJobMode(string JobToken, string Mode)
        {
            throw new NotImplementedException();
        }

        public virtual void SetTrackConfiguration(string RecordingToken, string TrackToken, TrackConfiguration TrackConfiguration)
        {
            throw new NotImplementedException();
        }

        public virtual StopExportRecordedDataResponse StopExportRecordedData(StopExportRecordedDataRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
