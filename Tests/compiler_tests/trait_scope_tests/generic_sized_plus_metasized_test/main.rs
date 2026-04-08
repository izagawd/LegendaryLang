fn identity[T:! Sized + MetaSized](x: T) -> T { x }
fn main() -> i32 { identity(42) }
