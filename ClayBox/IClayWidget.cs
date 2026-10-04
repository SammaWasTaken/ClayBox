using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClayBox
{
    public interface IClayWidget
    {
        public void OnEvent(ClayTextEditor textbox, ClayEventType eventType);

        public ClayImage GetImage();
    }

    public enum ClayEventType
    {
        TextChanged,
        ScrollChanged,
        WidgetAdded,
        Resized
    }
}
