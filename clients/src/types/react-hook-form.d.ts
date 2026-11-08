declare module "react-hook-form" {
  export type FieldValues = Record<string, unknown>;

  export interface FieldError {
    type?: string;
    message?: string;
  }

  export type FieldErrors<TFieldValues extends FieldValues> = Partial<
    Record<keyof TFieldValues | "root", FieldError>
  >;

  export interface UseFormProps<TFieldValues extends FieldValues> {
    resolver?: unknown;
    defaultValues?: Partial<TFieldValues>;
  }

  export interface UseFormReturn<TFieldValues extends FieldValues> {
    register: (name: keyof TFieldValues) => Record<string, unknown>;
    handleSubmit: (
      onValid: (values: TFieldValues) => void | Promise<void>,
    ) => (event?: unknown) => Promise<void>;
    formState: {
      errors: FieldErrors<TFieldValues>;
      isSubmitting: boolean;
    };
    setValue: <TName extends keyof TFieldValues>(
      name: TName,
      value: TFieldValues[TName],
      options?: unknown,
    ) => void;
    setError: (name: keyof TFieldValues | "root", error: FieldError) => void;
  }

  export function useForm<TFieldValues extends FieldValues>(
    props?: UseFormProps<TFieldValues>,
  ): UseFormReturn<TFieldValues>;
}
