"use client";

import { useEffect, useState } from "react";
import Markdown from "react-markdown";
import { Loader2 } from "lucide-react";
import { Callout } from "fumadocs-ui/components/callout";
import { GITHUB_RAW_URL, GITHUB_URL } from "@/lib/github";

const CHANGELOG_URL = `${GITHUB_RAW_URL}/CHANGELOG.md`;

const texts = {
  en: {
    failedToLoad: "Failed to load changelog",
    viewOnGitHub: "View on GitHub",
    loading: "Loading changelog...",
  },
  ja: {
    failedToLoad: "変更履歴の読み込みに失敗しました",
    viewOnGitHub: "GitHubで見る",
    loading: "変更履歴を読み込み中...",
  },
};

interface ChangelogContentProps {
  locale?: "en" | "ja";
}

export function ChangelogContent({ locale = "en" }: ChangelogContentProps) {
  const [content, setContent] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const t = texts[locale];

  useEffect(() => {
    fetch(CHANGELOG_URL)
      .then((res) => {
        if (!res.ok) throw new Error(`Failed to fetch: ${res.status}`);
        return res.text();
      })
      .then((text) => {
        // Remove the first "# Changelog" title
        setContent(text.replace(/^# Changelog\n+/, ""));
      })
      .catch((e) => setError(e.message));
  }, []);

  if (error) {
    return (
      <Callout type="error" title={t.failedToLoad}>
        {error}.{" "}
        <a
          href={`${GITHUB_URL}/blob/main/CHANGELOG.md`}
          target="_blank"
          rel="noopener noreferrer"
          className="underline"
        >
          {t.viewOnGitHub}
        </a>
      </Callout>
    );
  }

  if (!content) {
    return (
      <div className="flex items-center gap-2 text-fd-muted-foreground">
        <Loader2 className="size-4 animate-spin" />
        <span>{t.loading}</span>
      </div>
    );
  }

  return (
    <div className="prose prose-neutral dark:prose-invert max-w-none pb-18">
      <Markdown>{content}</Markdown>
    </div>
  );
}
