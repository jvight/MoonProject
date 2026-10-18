using System;

namespace MoonProject.UI
{
    /// <summary>
    /// Which pickup rows hold a story part (pure, EditMode-tested). A part is taken once, so its row shows no count:
    /// it opens on a free row (or the one that has rested longest), rests for the linger time and is released once it
    /// has faded. Taking the same part again feeds its own row instead of opening a second.
    /// </summary>
    internal sealed class PartRows
    {
        private readonly string[] _parts;
        private readonly float[] _age;
        private readonly float _lingerSeconds;

        public PartRows(int count, float lingerSeconds)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "At least one part row.");
            }

            _parts = new string[count];
            _age = new float[count];
            _lingerSeconds = lingerSeconds;
        }

        public int Count => _parts.Length;

        public bool IsActive(int row)
        {
            return _parts[row] != null;
        }

        public string PartOf(int row)
        {
            return _parts[row];
        }

        /// <summary>True once the row has rested for the linger time and should fade.</summary>
        public bool ShouldFade(int row)
        {
            return _parts[row] != null && _age[row] >= _lingerSeconds;
        }

        /// <summary>The row <paramref name="partId"/> shows on; <paramref name="opened"/> is false when it fed its own row.</summary>
        public int Claim(string partId, out bool opened)
        {
            if (string.IsNullOrEmpty(partId))
            {
                throw new ArgumentException("A part row needs a part id.", nameof(partId));
            }

            for (int i = 0; i < _parts.Length; i++)
            {
                if (string.Equals(_parts[i], partId, StringComparison.Ordinal))
                {
                    _age[i] = 0f;
                    opened = false;
                    return i;
                }
            }

            int row = 0;
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null)
                {
                    row = i;
                    break;
                }

                if (_age[i] > _age[row])
                {
                    row = i;
                }
            }

            _parts[row] = partId;
            _age[row] = 0f;
            opened = true;
            return row;
        }

        /// <summary>The row has faded out: it is free again.</summary>
        public void Release(int row)
        {
            _parts[row] = null;
            _age[row] = 0f;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] != null)
                {
                    _age[i] += deltaTime;
                }
            }
        }
    }
}
