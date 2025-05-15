export class CFAppError extends Error {
    public code!: string;
    public traceId?: string;
    constructor(code: string, message: string, traceId?: string | undefined) {
      super(message);
      this.code = code;
      this.name = code;
      this.traceId = traceId;
    }
  }
  