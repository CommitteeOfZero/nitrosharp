using System;

namespace NitroSharp.FreeTypePlayground
{
    public class Sdf
    {
        static double edgedf(double gx, double gy, double a)
        {
            double df, glength, temp, a1;

            if ((gx == 0) || (gy == 0))
            { // Either A) gu or gv are zero, or B) both
                df = 0.5 - a;  // Linear approximation is A) correct or B) a fair guess
            }
            else
            {
                glength = Math.Sqrt(gx * gx + gy * gy);
                if (glength > 0)
                {
                    gx = gx / glength;
                    gy = gy / glength;
                }
                /* Everything is symmetric wrt sign and transposition,
                 * so move to first octant (gx>=0, gy>=0, gx>=gy) to
                 * avoid handling all possible edge directions.
                 */
                gx = Math.Abs(gx);
                gy = Math.Abs(gy);
                if (gx < gy)
                {
                    temp = gx;
                    gx = gy;
                    gy = temp;
                }
                a1 = 0.5 * gy / gx;
                if (a < a1)
                { // 0 <= a < a1
                    df = 0.5 * (gx + gy) - Math.Sqrt(2.0 * gx * gy * a);
                }
                else if (a < (1.0 - a1))
                { // a1 <= a <= 1-a1
                    df = (0.5 - a) * gx;
                }
                else
                { // 1-a1 < a <= 1
                    df = -0.5 * (gx + gy) + Math.Sqrt(2.0 * gx * gy * (1.0 - a));
                }
            }
            return df;
        }

        static double distaa3(Span<double> img, Span<double> gximg, Span<double> gyimg, int w, int c, int xc, int yc, int xi, int yi)
        {
            double di, df, dx, dy, gx, gy, a;
            int closest;

            closest = c - xc - yc * w; // Index to the edge pixel pointed to from c
            a = img[closest];    // Grayscale value at the edge pixel
            gx = gximg[closest]; // X gradient component at the edge pixel
            gy = gyimg[closest]; // Y gradient component at the edge pixel

            if (a > 1.0) a = 1.0;
            if (a < 0.0) a = 0.0; // Clip grayscale values outside the range [0,1]
            if (a == 0.0) return 1000000.0; // Not an object pixel, return "very far" ("don't know yet")

            dx = xi;
            dy = yi;
            di = Math.Sqrt(dx * dx + dy * dy); // Length of integer vector, like a traditional EDT
            if (di == 0)
            { // Use local gradient only at edges
              // Estimate based on local gradient only
                df = edgedf(gx, gy, a);
            }
            else
            {
                // Estimate gradient based on direction to edge (accurate for large di)
                df = edgedf(dx, dy, a);
            }
            return di + df; // Same metric as edtaa2, except at edges (where di=0)
        }

        static void edtaa3(Span<double> img, Span<double> gx, Span<double> gy, int w, int h, Span<short> distx, Span<short> disty, Span<double> dist)
        {
            int x, y, i, c;
            int offset_u, offset_ur, offset_r, offset_rd,
            offset_d, offset_dl, offset_l, offset_lu;
            double olddist, newdist;
            int cdistx, cdisty, newdistx, newdisty;
            int changed;
            double epsilon = 1e-3;

            /* Initialize index offsets for the current image width */
            offset_u = -w;
            offset_ur = -w + 1;
            offset_r = 1;
            offset_rd = w + 1;
            offset_d = w;
            offset_dl = w - 1;
            offset_l = -1;
            offset_lu = -w - 1;

            /* Initialize the distance images */
            for (i = 0; i < w * h; i++)
            {
                distx[i] = 0; // At first, all pixels point to
                disty[i] = 0; // themselves as the closest known.
                if (img[i] <= 0.0)
                {
                    dist[i] = 1000000.0; // Big value, means "not set yet"
                }
                else if (img[i] < 1.0)
                {
                    dist[i] = edgedf(gx[i], gy[i], img[i]); // Gradient-assisted estimate
                }
                else
                {
                    dist[i] = 0.0; // Inside the object
                }
            }

            //double distaa3(img, gx, gy, w, int c, int xc, int yc, int xi, int yi)
            //    => distaa3(img, gx, gy, w, c, xc, yc, xi, yi);

            /* Perform the transformation */
            do
            {
                changed = 0;

                /* Scan rows, except first row */
                for (y = 1; y < h; y++)
                {

                    /* move index to leftmost pixel of current row */
                    i = y * w;

                    /* scan right, propagate distances from above & left */

                    /* Leftmost pixel is special, has no left neighbors */
                    olddist = dist[i];
                    if (olddist > 0) // If non-zero distance or not set yet
                    {
                        c = i + offset_u; // Index of candidate for testing
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx;
                        newdisty = cdisty + 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_ur;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx - 1;
                        newdisty = cdisty + 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            changed = 1;
                        }
                    }
                    i++;

                    /* Middle pixels have all neighbors */
                    for (x = 1; x < w - 1; x++, i++)
                    {
                        olddist = dist[i];
                        if (olddist <= 0) continue; // No need to update further

                        c = i + offset_l;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx + 1;
                        newdisty = cdisty;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_lu;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx + 1;
                        newdisty = cdisty + 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_u;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx;
                        newdisty = cdisty + 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_ur;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx - 1;
                        newdisty = cdisty + 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            changed = 1;
                        }
                    }

                    /* Rightmost pixel of row is special, has no right neighbors */
                    olddist = dist[i];
                    if (olddist > 0) // If not already zero distance
                    {
                        c = i + offset_l;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx + 1;
                        newdisty = cdisty;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_lu;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx + 1;
                        newdisty = cdisty + 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_u;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx;
                        newdisty = cdisty + 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            changed = 1;
                        }
                    }

                    /* Move index to second rightmost pixel of current row. */
                    /* Rightmost pixel is skipped, it has no right neighbor. */
                    i = y * w + w - 2;

                    /* scan left, propagate distance from right */
                    for (x = w - 2; x >= 0; x--, i--)
                    {
                        olddist = dist[i];
                        if (olddist <= 0) continue; // Already zero distance

                        c = i + offset_r;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx - 1;
                        newdisty = cdisty;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            changed = 1;
                        }
                    }
                }

                /* Scan rows in reverse order, except last row */
                for (y = h - 2; y >= 0; y--)
                {
                    /* move index to rightmost pixel of current row */
                    i = y * w + w - 1;

                    /* Scan left, propagate distances from below & right */

                    /* Rightmost pixel is special, has no right neighbors */
                    olddist = dist[i];
                    if (olddist > 0) // If not already zero distance
                    {
                        c = i + offset_d;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx;
                        newdisty = cdisty - 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_dl;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx + 1;
                        newdisty = cdisty - 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            changed = 1;
                        }
                    }
                    i--;

                    /* Middle pixels have all neighbors */
                    for (x = w - 2; x > 0; x--, i--)
                    {
                        olddist = dist[i];
                        if (olddist <= 0) continue; // Already zero distance

                        c = i + offset_r;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx - 1;
                        newdisty = cdisty;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_rd;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx - 1;
                        newdisty = cdisty - 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_d;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx;
                        newdisty = cdisty - 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_dl;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx + 1;
                        newdisty = cdisty - 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            changed = 1;
                        }
                    }
                    /* Leftmost pixel is special, has no left neighbors */
                    olddist = dist[i];
                    if (olddist > 0) // If not already zero distance
                    {
                        c = i + offset_r;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx - 1;
                        newdisty = cdisty;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_rd;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx - 1;
                        newdisty = cdisty - 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            olddist = newdist;
                            changed = 1;
                        }

                        c = i + offset_d;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx;
                        newdisty = cdisty - 1;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            changed = 1;
                        }
                    }

                    /* Move index to second leftmost pixel of current row. */
                    /* Leftmost pixel is skipped, it has no left neighbor. */
                    i = y * w + 1;
                    for (x = 1; x < w; x++, i++)
                    {
                        /* scan right, propagate distance from left */
                        olddist = dist[i];
                        if (olddist <= 0) continue; // Already zero distance

                        c = i + offset_l;
                        cdistx = distx[c];
                        cdisty = disty[c];
                        newdistx = cdistx + 1;
                        newdisty = cdisty;
                        newdist = distaa3(img, gx, gy, w, c, cdistx, cdisty, newdistx, newdisty);
                        if (newdist < olddist - epsilon)
                        {
                            distx[i] = (short)newdistx;
                            disty[i] = (short)newdisty;
                            dist[i] = newdist;
                            changed = 1;
                        }
                    }
                }
            }
            while (changed == 1); // Sweep until no more updates are made

            /* The transformation is completed. */

        }

        static void computegradient(ReadOnlySpan<double> img, int w, int h, Span<double> gx, Span<double> gy)
        {
            int i, j, k, p, q;
            double glength, phi, phiscaled, ascaled, errsign, pfrac, qfrac, err0, err1, err;
            const double SQRT2 = 1.4142136;
            for (i = 1; i < h - 1; i++)
            { // Avoid edges where the kernels would spill over
                for (j = 1; j < w - 1; j++)
                {
                    k = i * w + j;
                    if ((img[k] > 0.0) && (img[k] < 1.0))
                    { // Compute gradient for edge pixels only
                        gx[k] = -img[k - w - 1] - SQRT2 * img[k - 1] - img[k + w - 1] + img[k - w + 1] + SQRT2 * img[k + 1] + img[k + w + 1];
                        gy[k] = -img[k - w - 1] - SQRT2 * img[k - w] - img[k - w + 1] + img[k + w - 1] + SQRT2 * img[k + w] + img[k + w + 1];
                        glength = gx[k] * gx[k] + gy[k] * gy[k];
                        if (glength > 0.0)
                        { // Avoid division by zero
                            glength = Math.Sqrt(glength);
                            gx[k] = gx[k] / glength;
                            gy[k] = gy[k] / glength;
                        }
                    }
                }
            }
            // TODO: Compute reasonable values for gx, gy also around the image edges.
            // (These are zero now, which reduces the accuracy for a 1-pixel wide region
            // around the image edge.) 2x2 kernels would be suitable for this.
        }

        static void make_distance_mapd(Span<double> data, int width, int height)
        {
            Span<short> xdist = stackalloc short[(width * height)];
            Span<short> ydist = stackalloc short[(width * height)];
            Span<double> gx = stackalloc double[(width * height)];
            Span<double> gy = stackalloc double[(width * height)];
            Span<double> outside = stackalloc double[(width * height)];
            Span<double> inside = stackalloc double[(width * height)];
            double vmin = double.MaxValue;
            int i;

            // Compute outside = edtaa3(bitmap); % Transform background (0's)
            computegradient(data, width, height, gx, gy);
            edtaa3(data, gx, gy, width, height, xdist, ydist, outside);
            for (i = 0; i < width * height; ++i)
                if (outside[i] < 0.0)
                    outside[i] = 0.0;

            // Compute inside = edtaa3(1-bitmap); % Transform foreground (1's)
            gx.Clear();
            gy.Clear();

            for (i = 0; i < width * height; ++i)
                data[i] = 1 - data[i];
            computegradient(data, width, height, gx, gy);
            edtaa3(data, gx, gy, width, height, xdist, ydist, inside);
            for (i = 0; i < width * height; ++i)
                if (inside[i] < 0)
                    inside[i] = 0.0;

            // distmap = outside - inside; % Bipolar distance field
            for (i = 0; i < width * height; ++i)
            {
                outside[i] -= inside[i];
                if (outside[i] < vmin)
                    vmin = outside[i];
            }

            vmin = Math.Abs(vmin);

            for (i = 0; i < width * height; ++i)
            {
                double v = outside[i];
                if (v < -vmin) outside[i] = -vmin;
                else if (v > +vmin) outside[i] = +vmin;
                data[i] = (outside[i] + vmin) / (2 * vmin);
            }
        }

        public static unsafe void make_distance_mapb(ReadOnlySpan<byte> img, int width, int height, Span<byte> output)
        {
            Span<double> data = stackalloc double[(width * height)];
            int i;

            // find minimimum and maximum values
            double img_min = double.MaxValue;
            double img_max = double.MinValue;

            for (i = 0; i < width * height; ++i)
            {
                double v = img[i];
                data[i] = v;
                if (v > img_max)
                {
                    img_max = v;
                }
                if (v < img_min)
                {
                    img_min = v;
                }
            }

            // Map values from 0 - 255 to 0.0 - 1.0
            for (i = 0; i < width * height; ++i)
            {
                data[i] = (img[i] - img_min) / img_max;
            }

            make_distance_mapd(data, width, height);

            // map values from 0.0 - 1.0 to 0 - 255
            for (i = 0; i < width * height; ++i)
            {
                output[i] = (byte)(255 * (1 - data[i]));
            }
        }
    }
}
