using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StitchingImage.Stitch_Tools.RobotManager;

namespace StitchingImage.Stitch_Tools.Utils
{
    public sealed class ImageStore
    {
        private readonly Dictionary<int, GroupBucket> _groups = new Dictionary<int, GroupBucket>();
        private readonly Dictionary<string, ImageInfo> _byPath = new Dictionary<string, ImageInfo>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<int> GroupIds => _groups.Keys.OrderBy(x => x).ToArray();

        private sealed class GroupBucket
        {
            private readonly Dictionary<int, ImageInfo> _byId = new Dictionary<int, ImageInfo>();
            private readonly List<ImageInfo> _all = new List<ImageInfo>();

            public GroupBucket(int groupId) { GroupId = groupId; }
            public int GroupId { get; }

            public void Add(ImageInfo info)
            {
                _all.Add(info);
            }

            public bool TryGetById(int id, out ImageInfo info) => _byId.TryGetValue(id, out info);

            public ImageInfo[] GetAllSortedById()
            {
                // stable + easy debug
                return _all.OrderBy(x => x.ImageId).ToArray();
            }
        }

        public void Clear()
        {
            _groups.Clear();
            _byPath.Clear();
        }

        public void Add(ImageInfo info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            _byPath[info.FilePath] = info;

            GroupBucket bucket;
            if (!_groups.TryGetValue(info.GroupId, out bucket))
            {
                bucket = new GroupBucket(info.GroupId);
                _groups.Add(info.GroupId, bucket);
            }

            bucket.Add(info);
        }

        public bool TryGetGroup(int groupId, out ImageInfo[] imagesSortedById)
        {
            imagesSortedById = null;
            GroupBucket bucket;
            if (!_groups.TryGetValue(groupId, out bucket)) return false;
            imagesSortedById = bucket.GetAllSortedById();
            return true;
        }

        public bool TryGetByPath(string path, out ImageInfo info) => _byPath.TryGetValue(path, out info);

        public bool TryGetById(int groupId, int imageId, out ImageInfo info)
        {
            info = null;
            GroupBucket bucket;
            if (!_groups.TryGetValue(groupId, out bucket)) return false;
            return bucket.TryGetById(imageId, out info);
        }
    }
}
