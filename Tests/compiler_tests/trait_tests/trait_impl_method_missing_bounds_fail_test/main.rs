trait Foo {
    fn bro[T:! Sized +Copy](to_add: T) -> i32;
}
impl Foo for i32 {
    fn bro[T:! Sized](to_add: T) -> i32 {
        5
    }
}
fn main() -> i32 {
    0
}
