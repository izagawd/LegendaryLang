enum Wrapper[T:! MetaSized] {
    Some(T),
    None
}
fn consume[T:! MetaSized](w: Wrapper(T)) -> i32 { 0 }
fn main() -> i32 { 0 }
