using System.Collections.Generic;
using WowPacketParser.Messages.Submessages;

namespace WowPacketParser.Messages.Client
{
    public unsafe struct ClientPetBattleDebugQueueDumpResponse
    {
        public List<PBQueueDumpMember> Members;
        public UnixTime AverageQueueTime;
    }
}