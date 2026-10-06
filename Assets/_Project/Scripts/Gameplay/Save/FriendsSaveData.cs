using System;

namespace MoonProject.Gameplay
{
    /// <summary>Save section "gameplay.friends" (JsonUtility DTO): every friend's progress, matched by id.</summary>
    [Serializable]
    public sealed class FriendsSaveData
    {
        public FriendSaveData[] friends = Array.Empty<FriendSaveData>();
    }
}
