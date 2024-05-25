using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.DoorControl
{
    [DisableMustUnderstandValidation]
    public class DoorControlBase : DoorControlPort
    {
        public virtual AccessDoorResponse AccessDoor(AccessDoorRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void BlockDoor(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual string CreateDoor(Door Door)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteDoor(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void DoubleLockDoor(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DoorInfo")]
        public virtual GetDoorInfoResponse GetDoorInfo(GetDoorInfoRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetDoorInfoListResponse GetDoorInfoList(GetDoorInfoListRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetDoorListResponse GetDoorList(GetDoorListRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Door")]
        public virtual GetDoorsResponse GetDoors(GetDoorsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DoorState")]
        public virtual DoorState GetDoorState(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual ServiceCapabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        public virtual void LockDoor(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void LockDownDoor(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void LockDownReleaseDoor(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void LockOpenDoor(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void LockOpenReleaseDoor(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void ModifyDoor(Door Door)
        {
            throw new NotImplementedException();
        }

        public virtual void SetDoor(Door Door)
        {
            throw new NotImplementedException();
        }

        public virtual void UnlockDoor(string Token)
        {
            throw new NotImplementedException();
        }
    }
}
