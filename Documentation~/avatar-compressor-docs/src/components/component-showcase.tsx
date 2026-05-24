'use client';

import { useCallback, useEffect, useState } from 'react';
import Image from 'next/image';

export type ShowcaseItem =
  | { src: string; alt: string; width: number; height: number }
  | { placeholder: true };

const PLACEHOLDER_LABEL = 'In development.';

type Role = 'active' | 'prev' | 'next' | 'farPrev' | 'farNext';

// Five-column choreography: farPrev | prev | active | next | farNext.
// `far*` slots sit further out, further back, smaller, and fully transparent
// — items beyond ±1 from active stack onto the nearer far slot.
const ROLE_STYLE: Record<Role, { transform: string; opacity: number; blur: string; z: string }> = {
  active:  { transform: 'translate3d(0,0,0) scale(1)',         opacity: 1,    blur: 'blur(0)',   z: 'z-20' },
  prev:    { transform: 'translate3d(-55%,3%,0) scale(0.72)',  opacity: 0.35, blur: 'blur(2px)', z: 'z-10' },
  next:    { transform: 'translate3d(55%,3%,0) scale(0.72)',   opacity: 0.35, blur: 'blur(2px)', z: 'z-10' },
  farPrev: { transform: 'translate3d(-110%,6%,0) scale(0.5)',  opacity: 0,    blur: 'blur(4px)', z: 'z-0'  },
  farNext: { transform: 'translate3d(110%,6%,0) scale(0.5)',   opacity: 0,    blur: 'blur(4px)', z: 'z-0'  },
};

const CONTAINER_CLASS =
  'relative h-[400px] overflow-hidden sm:h-[500px] lg:h-[560px] ' +
  '[mask-image:linear-gradient(to_bottom,black_60%,transparent_100%)] ' +
  '[-webkit-mask-image:linear-gradient(to_bottom,black_60%,transparent_100%)] ' +
  'sm:[mask-image:linear-gradient(to_bottom,black_60%,transparent_100%),linear-gradient(to_right,transparent_0%,black_6%,black_94%,transparent_100%)] ' +
  'sm:[-webkit-mask-image:linear-gradient(to_bottom,black_60%,transparent_100%),linear-gradient(to_right,transparent_0%,black_6%,black_94%,transparent_100%)] ' +
  'sm:[mask-composite:intersect] sm:[-webkit-mask-composite:source-in]';
const LAYER_CLASS =
  'absolute inset-x-0 top-0 mx-auto w-full max-w-md transform-gpu transition-[transform,opacity,filter] duration-500 ease-out motion-reduce:transition-none';
const IMG_CLASS =
  'block aspect-[664/1607] w-full max-w-md rounded-lg border border-fd-border shadow-2xl';
const PLACEHOLDER_CLASS =
  'flex items-start justify-center bg-fd-background pt-[18%] text-fd-muted-foreground';
const BUTTON_CLASS =
  'block w-full appearance-none cursor-pointer rounded-lg border-0 bg-transparent p-0 focus:outline-none focus-visible:ring-2 focus-visible:ring-fd-primary';

export function ComponentShowcase({ items }: { items: ShowcaseItem[] }) {
  const [activeIdx, setActiveIdx] = useState(0);
  const count = items.length;
  const hasMultiple = count > 1;

  const cycle = useCallback(
    (dir: 1 | -1) => {
      setActiveIdx((idx) => (idx + dir + count) % count);
    },
    [count],
  );

  useEffect(() => {
    if (!hasMultiple) return;
    function onKey(e: KeyboardEvent) {
      const t = e.target as HTMLElement | null;
      if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.isContentEditable))
        return;
      if (e.key === 'ArrowLeft') cycle(-1);
      else if (e.key === 'ArrowRight') cycle(1);
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [cycle, hasMultiple]);

  function roleOf(i: number): Role {
    if (i === activeIdx) return 'active';
    if (!hasMultiple) return 'farNext';
    const fwd = ((i - activeIdx) % count + count) % count;
    const bwd = count - fwd;
    if (fwd === 1) return 'next';
    if (bwd === 1) return 'prev';
    // Stack remaining items at the nearer far slot; ties go to farNext.
    return fwd <= bwd ? 'farNext' : 'farPrev';
  }

  return (
    <div className={CONTAINER_CLASS}>
      {items.map((item, i) => {
        const role = roleOf(i);
        const style = ROLE_STYLE[role];
        const isClickable = role === 'prev' || role === 'next';
        const layerStyle = {
          transform: style.transform,
          opacity: style.opacity,
          filter: style.blur,
        };
        const isPlaceholder = 'placeholder' in item;
        const label = isPlaceholder ? PLACEHOLDER_LABEL : item.alt;
        const content = isPlaceholder ? (
          <div
            role="img"
            aria-label={PLACEHOLDER_LABEL}
            className={`${IMG_CLASS} ${PLACEHOLDER_CLASS}`}
          >
            <span className="text-base font-normal sm:text-lg lg:text-xl">
              {PLACEHOLDER_LABEL}
            </span>
          </div>
        ) : (
          <Image
            src={item.src}
            alt={item.alt}
            width={item.width}
            height={item.height}
            priority={role === 'active'}
            sizes="(min-width: 1024px) 28rem, 100vw"
            className={IMG_CLASS}
          />
        );

        return (
          <div
            key={i}
            aria-hidden={role === 'farPrev' || role === 'farNext' ? 'true' : undefined}
            className={`${LAYER_CLASS} ${style.z} ${isClickable ? '' : 'pointer-events-none'}`}
            style={layerStyle}
          >
            {isClickable ? (
              <button
                type="button"
                onClick={() => setActiveIdx(i)}
                aria-label={label}
                className={BUTTON_CLASS}
              >
                {content}
              </button>
            ) : (
              content
            )}
          </div>
        );
      })}
    </div>
  );
}
