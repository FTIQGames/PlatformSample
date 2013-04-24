using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace EnhancedPlatformer
{
    class Layer
    {

        public Texture2D[] Textures { get; private set; }
        public float ScrollRate { get; private set; }
        public float VerticalScrollRate { get; private set; }

        public Layer(ContentManager content, string basePath, float scrollRate, float verticalScrollRate)
        {
            // Assumes each layer only has 3 segments.
            Textures = new Texture2D[3];
            for (int i = 0; i < 3; ++i)
                Textures[i] = content.Load<Texture2D>(basePath + "_" + i);

            ScrollRate = scrollRate;
            VerticalScrollRate = verticalScrollRate;
        }

        public void Draw(SpriteBatch spriteBatch, float cameraPositionX, float cameraPositionY)
        {
            // Assumes each segment is the same width.
            int segmentWidth = Textures[0].Width;
            int segmentHeight = Textures[0].Height;

            // Calculate which segments to draw and how much to offset them.
            float x = cameraPositionX * ScrollRate;
            float y =  cameraPositionY * VerticalScrollRate;
            int leftSegment = (int)Math.Floor(x / segmentWidth);
            int rightSegment = leftSegment + 1;
            x = (x / segmentWidth - leftSegment) * -segmentWidth;

            spriteBatch.Draw(Textures[leftSegment % Textures.Length], new Vector2(x, y), Color.White);
            spriteBatch.Draw(Textures[rightSegment % Textures.Length], new Vector2(x + segmentWidth, y), Color.White);

        }

    }
}
