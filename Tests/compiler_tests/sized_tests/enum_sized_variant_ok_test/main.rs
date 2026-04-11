enum Wrapper(T:! Sized) {
    Some(T),
    None
}
fn consume[T:! Sized](w: Wrapper(T)) -> i32 { 0 }
fn main() -> i32 { consume(Wrapper.Some(42)) }
