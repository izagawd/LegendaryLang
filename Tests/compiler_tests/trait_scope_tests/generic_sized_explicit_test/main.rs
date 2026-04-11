fn identity[T:! Sized +Sized](x: T) -> T { x }
fn main() -> i32 { identity(42) }
