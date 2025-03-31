using Hypernex.CCK.Unity.Auth;
using HypernexSharp.APIObjects;

namespace Hypernex.Player
{
    public class APIPlayer
    {
        public static User APIUser => UserAuth.Instance.user;
    }
}