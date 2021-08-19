using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMapLibrary
{
    public class Dimension2D
    {
        private float _Width = 0;
        private float _Height = 0;
        public float Width
        {
            get => _Width;
            set
            {
                _Width = value;
                if (OnChanged != null)
                {
                    OnChanged(this);
                }
            }
        }
        public float Height
        {
            get => _Height;
            set
            {
                _Height = value;
                if (OnChanged != null)
                {
                    OnChanged(this);
                }
            }
        }

        public event Action<Dimension2D> OnChanged = null;

        public Dimension2D(float width, float height)
        {
            Width = width;
            Height = height;
        }
    }
}
