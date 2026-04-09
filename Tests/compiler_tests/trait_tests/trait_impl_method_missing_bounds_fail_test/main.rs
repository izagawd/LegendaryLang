trait Foo {
    fn bro[T:! Copy](to_add: T) -> i32;
}
impl Foo for i32 {
    fn bro[T:! type](to_add: T) -> i32 {
        5
    }
}
fn main() -> i32 {
    0
}
