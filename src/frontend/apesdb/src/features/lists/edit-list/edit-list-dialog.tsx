import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from "react";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
  Input,
} from "@apesdb/ui";
import { ImageIcon, Library, Loader2, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { GamesListDetails } from "../lists.schemas";
import { useEditGamesList } from "./use-edit-list";

const maximumNameLength = 128;
const maximumPictureLength = 5 * 1024 * 1024;
const supportedPictureTypes = new Set(["image/jpeg", "image/png", "image/webp"]);

type EditListDialogProps = {
  teamId: string;
  list: GamesListDetails;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export function EditListDialog({ teamId, list, open, onOpenChange }: EditListDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [name, setName] = useState(list.name);
  const [picture, setPicture] = useState<File | null>(null);
  const [picturePreviewUrl, setPicturePreviewUrl] = useState<string | null>(null);
  const [pictureRemoved, setPictureRemoved] = useState(false);
  const [nameError, setNameError] = useState<string | null>(null);
  const [pictureError, setPictureError] = useState<string | null>(null);
  const editList = useEditGamesList(teamId, list.id);

  // Reset the form from the current list whenever the dialog is opened. This
  // adjusts state during render (no effect) so reopened dialogs never show
  // stale values after the list changed elsewhere.
  const [wasOpen, setWasOpen] = useState(open);
  if (open !== wasOpen) {
    setWasOpen(open);

    if (open) {
      resetFields();
    }
  }

  useEffect(() => {
    return () => {
      if (picturePreviewUrl !== null) {
        URL.revokeObjectURL(picturePreviewUrl);
      }
    };
  }, [picturePreviewUrl]);

  function resetFields() {
    setName(list.name);
    setPicture(null);
    setPicturePreviewUrl(null);
    setPictureRemoved(false);
    setNameError(null);
    setPictureError(null);

    if (fileInputRef.current !== null) {
      fileInputRef.current.value = "";
    }
  }

  function handleOpenChange(nextOpen: boolean) {
    if (editList.isPending && !nextOpen) {
      return;
    }

    if (!nextOpen) {
      resetFields();
      editList.reset();
    }

    onOpenChange(nextOpen);
  }

  function handleNameChange(event: ChangeEvent<HTMLInputElement>) {
    setName(event.target.value);
    setNameError(null);

    if (editList.isError) {
      editList.reset();
    }
  }

  function handlePictureChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];

    if (!file) {
      return;
    }

    if (!supportedPictureTypes.has(file.type)) {
      setPictureError("Choose a JPEG, PNG, or WebP image.");
      event.target.value = "";
      return;
    }

    if (file.size > maximumPictureLength) {
      setPictureError("The picture must be 5 MB or smaller.");
      event.target.value = "";
      return;
    }

    setPicture(file);
    setPicturePreviewUrl(URL.createObjectURL(file));
    setPictureRemoved(false);
    setPictureError(null);

    if (editList.isError) {
      editList.reset();
    }
  }

  function removePicture() {
    setPicture(null);
    setPicturePreviewUrl(null);
    setPictureRemoved(true);
    setPictureError(null);

    if (fileInputRef.current !== null) {
      fileInputRef.current.value = "";
    }

    if (editList.isError) {
      editList.reset();
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedName = name.trim();

    if (trimmedName.length === 0) {
      setNameError("Enter a list name.");
      return;
    }

    if (trimmedName.length > maximumNameLength) {
      setNameError(`List names must be ${maximumNameLength} characters or fewer.`);
      return;
    }

    if (pictureError !== null) {
      return;
    }

    setNameError(null);

    try {
      const updatedList = await editList.mutateAsync({
        name: trimmedName,
        picture,
        removePicture: picture === null && pictureRemoved,
      });

      toast.success(`${updatedList.name} updated`);
      resetFields();
      editList.reset();
      onOpenChange(false);
    } catch {
      // The mutation error is rendered below the fields.
    }
  }

  const hasVisiblePicture = picture !== null || (list.pictureUrl !== null && !pictureRemoved);
  const displayedPictureUrl = picturePreviewUrl ?? (pictureRemoved ? null : list.pictureUrl);
  const requestError = editList.error instanceof Error ? editList.error.message : null;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent showCloseButton={!editList.isPending}>
        <form className="grid gap-4" onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Edit list</DialogTitle>
            <DialogDescription>Update the name or picture of this list.</DialogDescription>
          </DialogHeader>

          <Field data-invalid={nameError !== null}>
            <FieldLabel htmlFor="edit-list-name">List name</FieldLabel>
            <Input
              id="edit-list-name"
              autoComplete="off"
              autoFocus
              disabled={editList.isPending}
              maxLength={maximumNameLength}
              onChange={handleNameChange}
              placeholder="List name"
              value={name}
              aria-invalid={nameError !== null}
              aria-describedby={nameError !== null ? "edit-list-name-error" : undefined}
            />
            {nameError !== null ? (
              <FieldError id="edit-list-name-error">{nameError}</FieldError>
            ) : null}
          </Field>

          <Field data-invalid={pictureError !== null}>
            <FieldLabel htmlFor="edit-list-picture">Picture</FieldLabel>
            <div className="flex items-center gap-3">
              <Avatar className="size-14 rounded-lg">
                <AvatarImage alt="List picture preview" src={displayedPictureUrl ?? undefined} />
                <AvatarFallback className="rounded-lg bg-muted text-muted-foreground">
                  {hasVisiblePicture ? (
                    <ImageIcon className="size-5" />
                  ) : (
                    <Library className="size-5" />
                  )}
                </AvatarFallback>
              </Avatar>
              <div className="grid min-w-0 flex-1 gap-2">
                <Input
                  ref={fileInputRef}
                  id="edit-list-picture"
                  accept="image/jpeg,image/png,image/webp"
                  disabled={editList.isPending}
                  onChange={handlePictureChange}
                  type="file"
                  aria-invalid={pictureError !== null}
                  aria-describedby="edit-list-picture-description"
                />
                {hasVisiblePicture || pictureError !== null ? (
                  <Button
                    className="w-fit"
                    disabled={editList.isPending}
                    onClick={removePicture}
                    size="sm"
                    type="button"
                    variant="ghost"
                  >
                    <Trash2 data-icon="inline-start" />
                    {hasVisiblePicture ? "Remove picture" : "Clear selection"}
                  </Button>
                ) : null}
              </div>
            </div>
            <FieldDescription id="edit-list-picture-description">
              JPEG, PNG, or WebP. Maximum size 5 MB.
            </FieldDescription>
            {pictureError !== null ? <FieldError>{pictureError}</FieldError> : null}
          </Field>

          {requestError !== null ? <FieldError>{requestError}</FieldError> : null}

          <DialogFooter>
            <Button
              disabled={editList.isPending}
              onClick={() => handleOpenChange(false)}
              type="button"
              variant="outline"
            >
              Cancel
            </Button>
            <Button disabled={editList.isPending} type="submit">
              {editList.isPending ? <Loader2 className="animate-spin" /> : null}
              {editList.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
