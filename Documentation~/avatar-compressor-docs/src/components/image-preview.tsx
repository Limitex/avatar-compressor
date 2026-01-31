"use client";

import { useState, useEffect, useRef, useCallback, useSyncExternalStore, type FC } from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";

interface SliceProps {
  src: string;
  index: number;
  total: number;
  isHovered: boolean;
  onMouseEnter: () => void;
  onMouseLeave: () => void;
}

const Slice: FC<SliceProps> = ({
  src,
  index,
  total,
  isHovered,
  onMouseEnter,
  onMouseLeave,
}) => {
  const yPercent = total > 1 ? (index / (total - 1)) * 100 : 50;

  const cornerClass =
    index === 0 ? "rounded-l-lg" : index === total - 1 ? "rounded-r-lg" : "";

  return (
    <div
      className={`h-full overflow-hidden relative ${cornerClass}`}
      style={{
        flex: isHovered ? 1.4 : 1,
        transition: "flex 0.5s cubic-bezier(0.25, 0.46, 0.45, 0.94)",
      }}
      onMouseEnter={onMouseEnter}
      onMouseLeave={onMouseLeave}
    >
      <div
        className="size-full bg-cover transition-transform duration-500 ease-out"
        style={{
          backgroundImage: `url(${src})`,
          backgroundPosition: `center ${yPercent}%`,
          transform: isHovered ? "scale(1.08)" : "scale(1)",
        }}
      />
      {index < total - 1 && (
        <div className="absolute right-0 inset-y-0 w-px bg-white/15 z-10" />
      )}
    </div>
  );
};

interface PreviewCardProps {
  src: string;
  sliceCount?: number;
  label?: string;
  height?: number;
  onClick?: () => void;
  className?: string;
}

const PreviewCard: FC<PreviewCardProps> = ({
  src,
  sliceCount = 4,
  label,
  height = 180,
  onClick,
  className = "",
}) => {
  const [hoveredSlice, setHoveredSlice] = useState(-1);
  const [isHovered, setIsHovered] = useState(false);

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onClick?.();
        }
      }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => {
        setIsHovered(false);
        setHoveredSlice(-1);
      }}
      className={[
        "not-prose",
        "w-full flex gap-0.5 cursor-pointer rounded-lg overflow-hidden relative",
        "outline-none focus-visible:ring-2 focus-visible:ring-fd-primary/40",
        "transition-all duration-300",
        "shadow-md ring-1 ring-fd-border",
        isHovered && "shadow-xl -translate-y-0.5",
        className,
      ]
        .filter(Boolean)
        .join(" ")}
      style={{ height }}
    >
      {Array.from({ length: sliceCount }).map((_, i) => (
        <Slice
          key={i}
          src={src}
          index={i}
          total={sliceCount}
          isHovered={hoveredSlice === i}
          onMouseEnter={() => setHoveredSlice(i)}
          onMouseLeave={() => setHoveredSlice(-1)}
        />
      ))}

      <div className="absolute bottom-0 inset-x-0 h-16 bg-gradient-to-t from-white/50 dark:from-black/50 to-transparent pointer-events-none rounded-b-lg" />

      <div
        className={[
          "absolute bottom-2.5 right-3 flex items-center gap-1.5",
          "text-white/70 text-[11px] uppercase tracking-wider",
          "pointer-events-none transition-opacity duration-300",
          isHovered ? "opacity-100" : "opacity-0",
        ].join(" ")}
      >
        <span>Full View</span>
        <svg
          width="14"
          height="14"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          aria-hidden="true"
        >
          <path d="M15 3h6v6M9 21H3v-6M21 3l-7 7M3 21l7-7" />
        </svg>
      </div>

      {label && (
        <div className="absolute top-2.5 left-3 bg-black/50 backdrop-blur-sm text-white/80 text-[10px] font-mono px-2 py-0.5 rounded tracking-widest">
          {label}
        </div>
      )}
    </div>
  );
};

type AnimState = "closed" | "entering" | "open" | "leaving";

interface PreviewModalProps {
  src: string | null;
  isOpen: boolean;
  onClose: () => void;
  alt?: string;
}

const ModalContent: FC<PreviewModalProps> = ({
  src,
  isOpen,
  onClose,
  alt = "Full size image",
}) => {
  const [animState, setAnimState] = useState<AnimState>("closed");
  const backdropRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isOpen) {
      setAnimState("entering");
      const raf = requestAnimationFrame(() => {
        requestAnimationFrame(() => setAnimState("open"));
      });
      return () => cancelAnimationFrame(raf);
    }

    if (animState === "open" || animState === "entering") {
      setAnimState("leaving");
      const timer = setTimeout(() => setAnimState("closed"), 400);
      return () => clearTimeout(timer);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen]);

  useEffect(() => {
    if (animState === "closed") return;
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = prev;
    };
  }, [animState]);

  useEffect(() => {
    if (!isOpen) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [isOpen, onClose]);

  if (animState === "closed") return null;

  const isVisible = animState === "open";

  return (
    <div
      ref={backdropRef}
      role="dialog"
      aria-modal="true"
      aria-label="Image viewer"
      onClick={(e) => {
        if (e.target === backdropRef.current) onClose();
      }}
      className={[
        "fixed inset-0 z-[9999] flex items-center justify-center p-6 sm:p-10",
        "transition-all duration-[400ms]",
        isVisible
          ? "bg-black/85 backdrop-blur-xl"
          : "bg-transparent backdrop-blur-none",
      ].join(" ")}
    >
      <button
        type="button"
        onClick={onClose}
        aria-label="Close image viewer"
        className={[
          "absolute top-4 right-4 z-10",
          "size-10 rounded-full",
          "border border-white/15 bg-white/[0.08]",
          "text-white/80 text-lg",
          "flex items-center justify-center",
          "cursor-pointer transition-all duration-200",
          "hover:bg-white/15 hover:border-white/30",
          isVisible ? "opacity-100" : "opacity-0",
        ].join(" ")}
      >
        <X size={20} />
      </button>

      <div
        className={[
          "max-h-[90vh] max-w-[90vw] overflow-auto rounded-xl",
          "shadow-2xl",
          "transition-all duration-[450ms]",
          "[transition-timing-function:cubic-bezier(0.16,1,0.3,1)]",
          isVisible
            ? "opacity-100 scale-100 translate-y-0"
            : "opacity-0 scale-[0.92] translate-y-5",
        ].join(" ")}
      >
        {src && (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={src}
            alt={alt}
            className="block max-h-[85vh] max-w-[85vw] object-contain rounded-xl"
          />
        )}
      </div>

      <p
        className={[
          "absolute bottom-4 left-1/2 -translate-x-1/2",
          "text-white/40 text-xs tracking-wider select-none",
          "transition-opacity duration-[400ms] delay-200",
          isVisible ? "opacity-100" : "opacity-0",
        ].join(" ")}
      >
        Press ESC or click outside to close
      </p>
    </div>
  );
};

const emptySubscribe = () => () => {};

const PreviewModal: FC<PreviewModalProps> = (props) => {
  const isClient = useSyncExternalStore(emptySubscribe, () => true, () => false);
  if (!isClient) return null;
  return createPortal(<ModalContent {...props} />, document.body);
};

export interface ImagePreviewProps {
  src: string;
  alt?: string;
  sliceCount?: number;
  label?: string;
  height?: number;
  className?: string;
}

export const ImagePreview: FC<ImagePreviewProps> = ({
  src,
  alt,
  sliceCount,
  label,
  height,
  className,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const handleClose = useCallback(() => setIsOpen(false), []);

  return (
    <>
      <PreviewCard
        src={src}
        sliceCount={sliceCount}
        label={label}
        height={height}
        className={className}
        onClick={() => setIsOpen(true)}
      />
      <PreviewModal
        src={src}
        alt={alt ?? "Full size image"}
        isOpen={isOpen}
        onClose={handleClose}
      />
    </>
  );
};
