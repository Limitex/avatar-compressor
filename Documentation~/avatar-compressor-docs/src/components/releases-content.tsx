"use client";

import { useEffect, useState, useRef, useCallback } from "react";
import Markdown from "react-markdown";
import {
  Tag,
  Calendar,
  ExternalLink,
  ArrowRight,
  Download,
  HardDrive,
  Package,
  Loader2,
} from "lucide-react";
import { Callout } from "fumadocs-ui/components/callout";
import { GITHUB_API_URL, GITHUB_URL } from "@/lib/github";

const RELEASES_URL = `${GITHUB_API_URL}/releases`;
const PER_PAGE = 5;

const texts = {
  en: {
    failedToLoad: "Failed to load releases",
    failedToLoadMore: "Failed to load more",
    viewOnGitHub: "View on GitHub",
    viewAllOnGitHub: "View all releases on GitHub",
    loading: "Loading releases...",
    loadingMore: "Loading more...",
    noReleases: "No releases found.",
    latest: "Latest",
    preRelease: "Pre-release",
    installationGuide: "Installation Guide",
  },
  ja: {
    failedToLoad: "リリースの読み込みに失敗しました",
    failedToLoadMore: "追加の読み込みに失敗しました",
    viewOnGitHub: "GitHubで見る",
    viewAllOnGitHub: "GitHubで全リリースを見る",
    loading: "リリースを読み込み中...",
    loadingMore: "読み込み中...",
    noReleases: "リリースが見つかりません。",
    latest: "最新",
    preRelease: "プレリリース",
    installationGuide: "インストールガイド",
  },
};

interface Asset {
  id: number;
  name: string;
  browser_download_url: string;
  size: number;
  download_count: number;
}

interface Release {
  id: number;
  tag_name: string;
  name: string;
  body: string;
  published_at: string;
  html_url: string;
  assets: Asset[];
  prerelease: boolean;
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatDate(dateString: string, locale: "en" | "ja"): string {
  return new Date(dateString).toLocaleDateString(locale === "ja" ? "ja-JP" : "en-US", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

interface ReleasesContentProps {
  locale?: "en" | "ja";
}

export function ReleasesContent({ locale = "en" }: ReleasesContentProps) {
  const [releases, setReleases] = useState<Release[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [loadMoreError, setLoadMoreError] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const pageRef = useRef(1);
  const loadingMoreRef = useRef(false);
  const observerRef = useRef<IntersectionObserver | null>(null);
  const t = texts[locale];

  const fetchReleases = useCallback(async (pageNum: number) => {
    const url = `${RELEASES_URL}?per_page=${PER_PAGE}&page=${pageNum}`;
    const res = await fetch(url);
    if (!res.ok) throw new Error(`Failed to fetch: ${res.status}`);
    return res.json() as Promise<Release[]>;
  }, []);

  const loadMore = useCallback(async () => {
    if (loadingMoreRef.current) return;
    loadingMoreRef.current = true;
    setLoadingMore(true);
    setLoadMoreError(null);
    try {
      const nextPage = pageRef.current + 1;
      const data = await fetchReleases(nextPage);
      setReleases((prev) => [...prev, ...data]);
      pageRef.current = nextPage;
      setHasMore(data.length === PER_PAGE);
    } catch (e) {
      setLoadMoreError(e instanceof Error ? e.message : "Failed to load more");
      setHasMore(false);
    } finally {
      loadingMoreRef.current = false;
      setLoadingMore(false);
    }
  }, [fetchReleases]);

  const loadMoreRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }

      if (!node || loading || !hasMore) return;

      observerRef.current = new IntersectionObserver(
        (entries) => {
          if (entries[0].isIntersecting) {
            loadMore();
          }
        },
        { threshold: 0.1 },
      );

      observerRef.current.observe(node);
    },
    [loading, hasMore, loadMore],
  );

  useEffect(() => {
    fetchReleases(1)
      .then((data) => {
        setReleases(data);
        setHasMore(data.length === PER_PAGE);
        setLoading(false);
      })
      .catch((e) => {
        setError(e.message);
        setLoading(false);
      });
  }, [fetchReleases]);

  if (error) {
    return (
      <Callout type="error" title={t.failedToLoad}>
        {error}.{" "}
        <a
          href={`${GITHUB_URL}/releases`}
          target="_blank"
          rel="noopener noreferrer"
          className="underline"
        >
          {t.viewOnGitHub}
        </a>
      </Callout>
    );
  }

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-fd-muted-foreground">
        <Loader2 className="size-4 animate-spin" />
        <span>{t.loading}</span>
      </div>
    );
  }

  if (releases.length === 0) {
    return (
      <div className="rounded-lg border border-fd-border bg-fd-card p-8 text-center">
        <p className="text-fd-muted-foreground">{t.noReleases}</p>
      </div>
    );
  }

  return (
    <div className="divide-y divide-fd-border pb-18">
      {releases.map((release, index) => (
        <article key={release.id} className="py-6 first:pt-0 last:pb-0">
          <header className="mb-4">
            <h2 className="text-xl font-semibold text-fd-foreground">
              {release.name || release.tag_name}
            </h2>

            <div className="flex flex-wrap items-center gap-x-4 gap-y-2 mt-2 text-sm text-fd-muted-foreground">
              <span className="inline-flex items-center gap-1.5">
                <Tag className="size-3.5" />
                <code className="bg-fd-muted px-1.5 py-0.5 rounded text-xs font-mono">
                  {release.tag_name}
                </code>
              </span>
              <span className="inline-flex items-center gap-1.5">
                <Calendar className="size-3.5" />
                <time dateTime={release.published_at}>
                  {formatDate(release.published_at, locale)}
                </time>
              </span>
              {index === 0 && (
                <span className="bg-fd-primary text-fd-primary-foreground text-xs font-medium px-2 py-0.5 rounded-full">
                  {t.latest}
                </span>
              )}
              {release.prerelease && (
                <span className="bg-amber-500 text-white text-xs font-medium px-2 py-0.5 rounded-full">
                  {t.preRelease}
                </span>
              )}
              <a
                href={release.html_url}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1 hover:text-fd-primary transition-colors"
              >
                GitHub
                <ExternalLink className="size-3.5" />
              </a>
            </div>
          </header>

          {release.assets.length > 0 && (
            <div className="mb-4 space-y-2">
              <div className="flex flex-wrap gap-2">
                {release.assets.map((asset) => (
                  <div
                    key={asset.id}
                    className="flex flex-col gap-1 text-sm bg-fd-muted px-3 py-2 rounded-lg"
                  >
                    <span className="inline-flex items-center gap-2">
                      <Package className="size-3.5 text-fd-muted-foreground flex-shrink-0" />
                      <span>{asset.name}</span>
                    </span>
                    <span className="inline-flex items-center gap-1.5 text-fd-muted-foreground text-xs pl-5.5">
                      <span className="inline-flex items-center gap-1 bg-fd-background/50 px-2 py-0.5 rounded">
                        <HardDrive className="size-2.5" />
                        {formatBytes(asset.size)}
                      </span>
                      <span className="inline-flex items-center gap-1 bg-fd-background/50 px-2 py-0.5 rounded">
                        <Download className="size-2.5" />
                        {asset.download_count.toLocaleString()}
                      </span>
                    </span>
                  </div>
                ))}
              </div>
              <a
                href={`/${locale}/docs/installation`}
                className="inline-flex items-center gap-1.5 text-sm text-fd-primary hover:underline"
              >
                {t.installationGuide}
                <ArrowRight className="size-3.5" />
              </a>
            </div>
          )}

          {release.body && (
            <div className="prose prose-neutral dark:prose-invert max-w-none prose-sm">
              <Markdown>{release.body}</Markdown>
            </div>
          )}
        </article>
      ))}

      {loadMoreError && (
        <div className="py-4">
          <Callout type="warn" title={t.failedToLoadMore}>
            {loadMoreError}.{" "}
            <a
              href={`${GITHUB_URL}/releases`}
              target="_blank"
              rel="noopener noreferrer"
              className="underline"
            >
              {t.viewAllOnGitHub}
            </a>
          </Callout>
        </div>
      )}

      {hasMore && (
        <div ref={loadMoreRef} className="py-6 flex justify-center">
          {loadingMore && (
            <div className="flex items-center gap-2 text-fd-muted-foreground">
              <Loader2 className="size-4 animate-spin" />
              <span>{t.loadingMore}</span>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
