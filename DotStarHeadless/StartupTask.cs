/*------------------------------------------------------------------------
  Windows IoT Core demonstration app for Adafruit DotStar Strips

  Requires the Adafruit Class Library for Windows IoT Core

  Written by Rick Lesniak for Adafruit Industries.

  Adafruit invests time and resources providing this open source code,
  please support Adafruit and open-source hardware by purchasing products
  from Adafruit!

  ------------------------------------------------------------------------
  Adafruit DotStarHeadless is free software: you can redistribute it and/or
  modify it under the terms of the GNU Lesser General Public License
  as published by the Free Software Foundation, either version 3 of
  the License, or (at your option) any later version.

  Adafruit DotStarHeadless is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU Lesser General Public License for more details.

  You should have received a copy of the GNU Lesser General Public
  License along with DotStar.  If not, see <http://www.gnu.org/licenses/>.
  ------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using AdafruitClassLibrary;
using System.Threading.Tasks;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace StrandTest
{
    public sealed class StartupTask : IBackgroundTask
    {
        //DotStar strip = new DotStar(60, 12, 13, DotStar.DOTSTAR_BGR);    //software SPI.  Data on GPIO 12, Clock on GPIO 13
        DotStar strip = new DotStar(60, DotStar.DOTSTAR_BGR);    //hardware SPI.  Data on GPIO 10, Clock on GPIO 11

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            //    // If you start any asynchronous methods here, prevent the task
            //    // from closing prematurely by using BackgroundTaskDeferral as
            //    // described in http://aka.ms/backgroundtaskdeferral

            //
            // Create the deferral by requesting it from the task instance.
            //
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            //
            // Call asynchronous method(s) using the await keyword.
            //
            await strip.Begin().ConfigureAwait(false);

            while (true)
            {
                RGBRun(3, 20);  //Color wipes

                TheaterChase(strip.Color(127, 127, 127), 50, 10); // White
                TheaterChase(strip.Color(127, 0, 0), 50, 10); // Red
                TheaterChase(strip.Color(0, 0, 127), 50, 10); // Blue

                Rainbow(20);
                RainbowCycle(20);
                TheaterChaseRainbow(50);
            }

            //
            // Once the asynchronous method(s) are done, close the deferral.
            //
            deferral.Complete();

        }

        public void RGBRun(int iterations, int wait)
        {
            int Head = 0;
            int Tail = -10;
            UInt32 Color = strip.Color(0, 0, 255);  //start with blue

            strip.Brightness = 255;  //start at full brightness

            //turn all pixels off
            strip.Clear();
            strip.Show();

            //run the color wipes
            while (iterations > 0)
            {
                strip.SetPixelColor(Head, Color); // 'On' pixel at head
                strip.SetPixelColor(Tail, 0);     // 'Off' pixel at tail
                strip.Show();                     // Refresh strip

                Task.Delay(wait).Wait();

                if (++Head >= strip.NumPixels)
                {         // Increment head index.  Off end of strip?
                    Head = 0;                       //  Yes, reset head index to start
                    strip.Brightness = (byte)(strip.Brightness / 2);
                    if ((Color >>= 8) == 0)          //  Next color (R->G->B) ... past blue now?
                    {
                        Color = strip.Color(255, 0, 0);  //   Yes, reset to red
                        iterations--;
                    }
                }

                if (++Tail >= strip.NumPixels)
                {
                    Tail = 0; // Increment, reset tail index
                }
            }

            strip.Brightness = 255;  //Reset brightness

        }

        public void Rainbow(int wait)
        {
            for (int cycle = 0; cycle < 256; cycle++)
            {
                for (int pixel = 0; pixel < strip.NumPixels; pixel++)
                {
                    strip.SetPixelColor(pixel, Wheel((uint)(pixel + cycle) & 255));
                }
                strip.Show();
                Task.Delay(wait).Wait();
            }
        }

        // Slightly different, this makes the rainbow equally distributed throughout
        public void RainbowCycle(int wait)
        {
            for (int cycle = 0; cycle < 256 * 5; cycle++)
            {
                for (int pixel = 0; pixel < strip.NumPixels; pixel++)
                {
                    strip.SetPixelColor(pixel, Wheel((uint)((pixel * 256 / strip.NumPixels) + cycle) & 255));
                }
                strip.Show();
                Task.Delay(wait).Wait();
            }
        }

        //Theatre-style crawling lights 
        public void TheaterChase(uint color, int wait, int cycleCount)
        {
            for (int cycle = 0; cycle < cycleCount; cycle++)
            {
                for (int offset = 0; offset < 3; offset++)
                {
                    for (int pixel = 0; pixel < strip.NumPixels; pixel = pixel + 3)
                    {
                        strip.SetPixelColor(pixel + offset, color);    //turn every third pixel on
                    }
                    strip.Show();

                    Task.Delay(wait).Wait();

                    for (int pixel = 0; pixel < strip.NumPixels; pixel = pixel + 3)
                    {
                        strip.SetPixelColor(pixel + offset, 0);        //turn every third pixel off
                    }
                }
            }
        }

        //Theatre-style crawling lights with rainbow effect
        public void TheaterChaseRainbow(int wait)
        {
            for (int cycle = 0; cycle < 256; cycle++)  // cycle all 256 colors in the wheel
            {
                for (int offset = 0; offset < 3; offset++)
                {
                    for (int i = 0; i < strip.NumPixels; i = i + 3)
                    {
                        strip.SetPixelColor(i + offset, Wheel((uint)(i + cycle) % 255));    //turn every third pixel on
                    }
                    strip.Show();

                    Task.Delay(wait).Wait();

                    for (int i = 0; i < strip.NumPixels; i = i + 3)
                    {
                        strip.SetPixelColor(i + offset, 0);        //turn every third pixel off
                    }
                }
            }
        }

        // Input a value 0 to 255 to get a color value.
        // The colors are a transition r - g - b - back to r.
        public uint Wheel(uint WheelPos)
        {
            if (WheelPos < 85)
            {
                return strip.Color((WheelPos * 3), (255 - WheelPos * 3), 0);
            }
            else if (WheelPos < 170)
            {
                WheelPos -= 85;
                return strip.Color((255 - WheelPos * 3), 0, (WheelPos * 3));
            }
            else
            {
                WheelPos -= 170;
                return strip.Color(0, (WheelPos * 3), (255 - WheelPos * 3));
            }
        }
    }
}
