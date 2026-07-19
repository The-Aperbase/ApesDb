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
import { useCreateGamesList } from "./use-create-list";

const maximumNameLength = 128;
const maximumPictureLength = 5 * 1024 * 1024;
const supportedPictureTypes = new Set(["image/jpeg", "image/png", "image/webp"]);

type CreateListDialogProps = {
  teamId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: (listId: string) => void;
};

export function CreateListDialog({ teamId, open, onOpenChange, onCreated }: CreateListDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [name, setName] = useState("");
  const [picture, setPicture] = useState<File | null>(null);
  const [picturePreviewUrl, setPicturePreviewUrl] = useState<string | null>(null);
  const [nameError, setNameError] = useState<string | null>(null);
  const [pictureError, setPictureError] = useState<string | null>(null);
  const createList = useCreateGamesList(teamId);

  useEffect(() => {
    return () => {
      if (picturePreviewUrl !== null) {
        URL.revokeObjectURL(picturePreviewUrl);
      }
    };
  }, [picturePreviewUrl]);

  function resetForm() {
    setName("");
    setPicture(null);
    setPicturePreviewUrl(null);
    setNameError(null);
    setPictureError(null);
    createList.reset();

    if (fileInputRef.current !== null) {
      fileInputRef.current.value = "";
    }
  }

  function handleOpenChange(nextOpen: boolean) {
    if (createList.isPending && !nextOpen) {
      return;
    }

    if (!nextOpen) {
      resetForm();
    }

    onOpenChange(nextOpen);
  }

  function handleNameChange(event: ChangeEvent<HTMLInputElement>) {
    setName(event.target.value);
    setNameError(null);

    if (createList.isError) {
      createList.reset();
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
    setPictureError(null);

    if (createList.isError) {
      createList.reset();
    }
  }

  function removePicture() {
    setPicture(null);
    setPicturePreviewUrl(null);
    setPictureError(null);

    if (fileInputRef.current !== null) {
      fileInputRef.current.value = "";
    }

    if (createList.isError) {
      createList.reset();
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
      const createdList = await createList.mutateAsync({
        name: trimmedName,
        picture,
      });

      toast.success(`${createdList.name} created`);
      onCreated(createdList.id);
      resetForm();
      onOpenChange(false);
    } catch {
      // The mutation error is rendered below the fields.
    }
  }

  const requestError = createList.error instanceof Error ? createList.error.message : null;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent showCloseButton={!createList.isPending}>
        <form className="grid gap-4" onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Create list</DialogTitle>
            <DialogDescription>
              Create a games list that every member of this team can edit.
            </DialogDescription>
          </DialogHeader>

          <Field data-invalid={nameError !== null}>
            <FieldLabel htmlFor="create-list-name">List name</FieldLabel>
            <Input
              id="create-list-name"
              autoComplete="off"
              autoFocus
              disabled={createList.isPending}
              maxLength={maximumNameLength}
              onChange={handleNameChange}
              placeholder="List name"
              value={name}
              aria-invalid={nameError !== null}
              aria-describedby={nameError !== null ? "create-list-name-error" : undefined}
            />
            {nameError !== null ? (
              <FieldError id="create-list-name-error">{nameError}</FieldError>
            ) : null}
          </Field>

          <Field data-invalid={pictureError !== null}>
            <FieldLabel htmlFor="create-list-picture">Picture</FieldLabel>
            <div className="flex items-center gap-3">
              <Avatar className="size-14 rounded-lg">
                <AvatarImage alt="List picture preview" src={picturePreviewUrl ?? undefined} />
                <AvatarFallback className="rounded-lg bg-muted text-muted-foreground">
                  {picture === null ? (
                    <Library className="size-5" />
                  ) : (
                    <ImageIcon className="size-5" />
                  )}
                </AvatarFallback>
              </Avatar>
              <div className="grid min-w-0 flex-1 gap-2">
                <Input
                  ref={fileInputRef}
                  id="create-list-picture"
                  accept="image/jpeg,image/png,image/webp"
                  disabled={createList.isPending}
                  onChange={handlePictureChange}
                  type="file"
                  aria-invalid={pictureError !== null}
                  aria-describedby="create-list-picture-description"
                />
                {picture !== null || pictureError !== null ? (
                  <Button
                    className="w-fit"
                    disabled={createList.isPending}
                    onClick={removePicture}
                    size="sm"
                    type="button"
                    variant="ghost"
                  >
                    <Trash2 data-icon="inline-start" />
                    {picture !== null ? "Remove picture" : "Clear selection"}
                  </Button>
                ) : null}
              </div>
            </div>
            <FieldDescription id="create-list-picture-description">
              JPEG, PNG, or WebP. Maximum size 5 MB.
            </FieldDescription>
            {pictureError !== null ? <FieldError>{pictureError}</FieldError> : null}
          </Field>

          {requestError !== null ? <FieldError>{requestError}</FieldError> : null}

          <DialogFooter>
            <Button
              disabled={createList.isPending}
              onClick={() => handleOpenChange(false)}
              type="button"
              variant="outline"
            >
              Cancel
            </Button>
            <Button disabled={createList.isPending} type="submit" variant="outline">
              {createList.isPending ? <Loader2 className="animate-spin" /> : null}
              {createList.isPending ? "Creating…" : "Create list"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
