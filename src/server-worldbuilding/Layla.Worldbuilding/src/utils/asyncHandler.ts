import type { Request, Response, NextFunction } from "express";

/** Type alias for async Express controller/middleware functions. */
type AsyncController = (
  req: Request,
  res: Response,
  next: NextFunction,
) => Promise<void>;

/**
 * Wraps an async Express handler and forwards any rejected promise to `next`,
 * allowing the global error handler to process it.
 *
 * Required because Express 5 does not automatically propagate async errors
 * from handlers that do not call `next` on rejection.
 *
 * @example
 * router.get("/", asyncHandler(myController));
 */
export const asyncHandler =
  (fn: AsyncController) =>
  (req: Request, res: Response, next: NextFunction): void => {
    fn(req, res, next).catch(next);
  };
