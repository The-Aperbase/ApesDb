import { useEffect, useState, type ComponentProps } from "react";
import { Input } from "@apesdb/ui";

type DebouncedFilterInputProps = Omit<ComponentProps<typeof Input>, "onChange" | "value"> & {
  value: string;
  onValueChange: (value: string) => void;
};

export function DebouncedFilterInput({
  value,
  onValueChange,
  ...props
}: DebouncedFilterInputProps) {
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  useEffect(() => {
    if (draft === value) {
      return;
    }

    const timeout = window.setTimeout(() => onValueChange(draft), 300);
    return () => window.clearTimeout(timeout);
  }, [draft, onValueChange, value]);

  return <Input {...props} value={draft} onChange={(event) => setDraft(event.target.value)} />;
}
