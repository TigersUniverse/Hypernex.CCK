using System.Collections.Generic;
using Hypernex.Game.Avatar;
using UnityEngine;

namespace Hypernex.Game
{
    public class NetPlayer : MonoBehaviour, IPlayer
    {
        public string UserId => Id;
        public AvatarCreator Avatar => AvatarCreator;
        
        public bool IsLocal => false;
        public string Id { get; }
        public AvatarCreator AvatarCreator { get; }
        public bool IsLoadingAvatar { get; }
        public float AvatarDownloadPercentage { get; }

        public Dictionary<string, object> LastExtraneousObjects = new Dictionary<string, object>();
        public List<string> LastPlayerTags = new List<string>();
    }
}