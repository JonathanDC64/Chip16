using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip16
{
    /// <summary>
    /// Bit[0] = Up Bit[1] = Down Bit[2] = Left Bit[3] = Right Bit[4] = Select
    /// Bit[5] = Start Bit[6] = A Bit[7] = B Bit[8..15] = Unused(Always zero).
    /// The state of each controller(for each button, 1 = pressed, 0 = not pressed) is updated at every VBLANK event.
    /// Up to 2 controllers are supported for now; controller 1 updates addresses 0xFFF0-0xFFF1, controller 2 updates 0xFFF2-0xFFF3.
    /// </summary>
    public class Input
    {
        private bool[] input;

        public Input()
        {
            this.input = new bool[8];
        }

        public void SetState(bool[] input)
        {
            this.input = input;
        }

        public byte State
        {
            get
            {
                byte result = 0;
                // This assumes the array never contains more than 8 elements!
                int index = 8 - input.Length;

                // Loop through the array
                foreach (bool b in input)
                {
                    // if the element is 'true' set the bit at that position
                    if (b)
                        result |= (byte)(1 << index);

                    index++;
                }

                return result;
            }
        }
    }
}
