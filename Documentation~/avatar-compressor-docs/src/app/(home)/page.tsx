import Link from 'next/link';

export default function HomePage() {
  return (
    <div className="flex flex-col justify-center text-center flex-1">
      <h1 className="text-2xl font-bold mb-4">Avatar Compressor</h1>
      <p className="mb-4 text-fd-muted-foreground">
        Unity package for compressing VRChat avatars
      </p>
      <p>
        <Link href="/docs" className="font-medium underline">
          Documentation
        </Link>
      </p>
    </div>
  );
}
