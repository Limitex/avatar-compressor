"use client";

import React from "react";
import { PackagePlus } from "lucide-react";
import Button from "./button";

interface VPMRepositoryLinkProps {
  repoUrl: string;
  label?: string;
}

const VPMRepositoryLink: React.FC<VPMRepositoryLinkProps> = ({
  repoUrl,
  label = "Add Repository",
}) => {
  const vccUrl = `vcc://vpm/addRepo?url=${repoUrl}`;

  return (
    <a href={vccUrl} className="no-underline block">
      <Button size="lg" className="w-full gap-3">
        <PackagePlus size={20} />
        <span>{label}</span>
      </Button>
    </a>
  );
};

export default VPMRepositoryLink;
