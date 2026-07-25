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
import { ImageIcon, ImageUp, Library, Loader2, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { useCreateBoard } from "./use-create-board";

const maximumNameLength = 128;
const maximumPictureLength = 5 * 1024 * 1024;
const supportedPictureTypes = new Set(["image/jpeg", "image/png", "image/webp"]);

type CreateBoardDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: (boardId: string) => void;
};

export function CreateBoardDialog({ open, onOpenChange, onCreated }: CreateBoardDialogProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [name, setName] = useState("");
  const [picture, setPicture] = useState<File | null>(null);
  const [picturePreviewUrl, setPicturePreviewUrl] = useState<string | null>(null);
  const [nameError, setNameError] = useState<string | null>(null);
  const [pictureError, setPictureError] = useState<string | null>(null);
  const createBoard = useCreateBoard();

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
    createBoard.reset();

    if (fileInputRef.current !== null) {
      fileInputRef.current.value = "";
    }
  }

  function handleOpenChange(nextOpen: boolean) {
    if (createBoard.isPending && !nextOpen) {
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

    if (createBoard.isError) {
      createBoard.reset();
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

    if (createBoard.isError) {
      createBoard.reset();
    }
  }

  function removePicture() {
    setPicture(null);
    setPicturePreviewUrl(null);
    setPictureError(null);

    if (fileInputRef.current !== null) {
      fileInputRef.current.value = "";
    }

    if (createBoard.isError) {
      createBoard.reset();
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedName = name.trim();

    if (trimmedName.length === 0) {
      setNameError("Enter a board name.");
      return;
    }

    if (trimmedName.length > maximumNameLength) {
      setNameError(`Board names must be ${maximumNameLength} characters or fewer.`);
      return;
    }

    if (pictureError !== null) {
      return;
    }

    setNameError(null);

    try {
      const createdBoard = await createBoard.mutateAsync({
        name: trimmedName,
        picture,
      });

      toast.success(`${createdBoard.name} created`);
      onCreated(createdBoard.id);
      resetForm();
      onOpenChange(false);
    } catch {
      // The mutation error is rendered below the fields.
    }
  }

  const requestError = createBoard.error instanceof Error ? createBoard.error.message : null;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent showCloseButton={!createBoard.isPending}>
        <form className="grid gap-4" onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Create board</DialogTitle>
            <DialogDescription>Create a board to organize games by progress.</DialogDescription>
          </DialogHeader>

          <Field data-invalid={nameError !== null}>
            <FieldLabel htmlFor="create-board-name">Board name</FieldLabel>
            <Input
              id="create-board-name"
              autoComplete="off"
              autoFocus
              disabled={createBoard.isPending}
              maxLength={maximumNameLength}
              onChange={handleNameChange}
              placeholder="Board name"
              value={name}
              aria-invalid={nameError !== null}
              aria-describedby={nameError !== null ? "create-board-name-error" : undefined}
            />
            {nameError !== null ? (
              <FieldError id="create-board-name-error">{nameError}</FieldError>
            ) : null}
          </Field>

          <Field data-invalid={pictureError !== null}>
            <FieldLabel htmlFor="create-board-picture">Picture</FieldLabel>
            <div className="flex items-center gap-3">
              <Avatar className="size-14 rounded-lg">
                <AvatarImage alt="Board picture preview" src={picturePreviewUrl ?? undefined} />
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
                  className="sr-only"
                  id="create-board-picture"
                  accept="image/jpeg,image/png,image/webp"
                  disabled={createBoard.isPending}
                  onChange={handlePictureChange}
                  type="file"
                  aria-invalid={pictureError !== null}
                  aria-describedby="create-board-picture-description"
                />
                <Button
                  className="w-fit"
                  disabled={createBoard.isPending}
                  onClick={() => fileInputRef.current?.click()}
                  size="sm"
                  type="button"
                  variant="outline"
                >
                  <ImageUp data-icon="inline-start" />
                  Choose file
                </Button>
                {picture !== null || pictureError !== null ? (
                  <Button
                    className="w-fit"
                    disabled={createBoard.isPending}
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
            <FieldDescription id="create-board-picture-description">
              JPEG, PNG, or WebP. Maximum size 5 MB.
            </FieldDescription>
            {pictureError !== null ? <FieldError>{pictureError}</FieldError> : null}
          </Field>

          {requestError !== null ? <FieldError>{requestError}</FieldError> : null}

          <DialogFooter>
            <Button
              disabled={createBoard.isPending}
              onClick={() => handleOpenChange(false)}
              type="button"
              variant="outline"
            >
              Cancel
            </Button>
            <Button disabled={createBoard.isPending} type="submit" variant="outline">
              {createBoard.isPending ? <Loader2 className="animate-spin" /> : null}
              {createBoard.isPending ? "Creating…" : "Create board"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
