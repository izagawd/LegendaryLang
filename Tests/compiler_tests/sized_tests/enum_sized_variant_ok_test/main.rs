enum Wrapper(T:! type) {
    Some(T),
    None
}
fn consume[T:! type](w: Wrapper(T)) -> i32 { 0 }
fn main() -> i32 { consume(Wrapper.Some(42)) }
