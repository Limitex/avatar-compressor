"use client";

import React from "react";
import { sendGAEvent } from "@next/third-parties/google";
import { PackagePlus } from "lucide-react";
import Button from "./button";

interface VPMRepositoryLinkProps {
  repoUrl: string;
  label?: string;
  method?: "alcom" | "vcc";
  eventName?: string;
}

const VPMRepositoryLink: React.FC<VPMRepositoryLinkProps> = ({
  repoUrl,
  label = "Add Repository",
  method = "vcc",
  eventName = "add_repository",
}) => {
  const vccUrl = `vcc://vpm/addRepo?url=${repoUrl}`;

  const handleClick = () => {
    sendGAEvent("event", eventName, {
      method,
      repo_url: repoUrl,
    });
  };

  return (
    <a href={vccUrl} className="no-underline block" onClick={handleClick}>
      <Button size="lg" className="w-full gap-3">
        <PackagePlus size={20} />
        <span>{label}</span>
      </Button>
    </a>
  );
};

export default VPMRepositoryLink;
