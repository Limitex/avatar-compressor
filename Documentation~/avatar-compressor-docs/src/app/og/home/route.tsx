import { ImageResponse } from 'next/og';
import { generate as DefaultImage } from 'fumadocs-ui/og';

export const revalidate = false;

export async function GET() {
  return new ImageResponse(
    (
      <DefaultImage
        title="Avatar Compressor"
        description="VRChat avatar compression toolkit - Reduce file size and VRAM usage while preserving quality"
        site="Avatar Compressor"
      />
    ),
    {
      width: 1200,
      height: 630,
    },
  );
}
