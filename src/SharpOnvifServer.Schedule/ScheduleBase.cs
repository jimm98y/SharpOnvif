using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Schedule
{
    [DisableMustUnderstandValidation]
    public class ScheduleBase : SchedulePort
    {
        [return: MessageParameter(Name = "Token")]
        public virtual string CreateSchedule(Schedule Schedule)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual string CreateSpecialDayGroup(SpecialDayGroup SpecialDayGroup)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteSchedule(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteSpecialDayGroup(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ScheduleInfo")]
        public virtual GetScheduleInfoResponse GetScheduleInfo(GetScheduleInfoRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetScheduleInfoListResponse GetScheduleInfoList(GetScheduleInfoListRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetScheduleListResponse GetScheduleList(GetScheduleListRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Schedule")]
        public virtual GetSchedulesResponse GetSchedules(GetSchedulesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ScheduleState")]
        public virtual ScheduleState GetScheduleState(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual ServiceCapabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SpecialDayGroupInfo")]
        public virtual GetSpecialDayGroupInfoResponse GetSpecialDayGroupInfo(GetSpecialDayGroupInfoRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetSpecialDayGroupInfoListResponse GetSpecialDayGroupInfoList(GetSpecialDayGroupInfoListRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetSpecialDayGroupListResponse GetSpecialDayGroupList(GetSpecialDayGroupListRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SpecialDayGroup")]
        public virtual GetSpecialDayGroupsResponse GetSpecialDayGroups(GetSpecialDayGroupsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void ModifySchedule(Schedule Schedule)
        {
            throw new NotImplementedException();
        }

        public virtual void ModifySpecialDayGroup(SpecialDayGroup SpecialDayGroup)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSchedule(Schedule Schedule)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSpecialDayGroup(SpecialDayGroup SpecialDayGroup)
        {
            throw new NotImplementedException();
        }
    }
}
