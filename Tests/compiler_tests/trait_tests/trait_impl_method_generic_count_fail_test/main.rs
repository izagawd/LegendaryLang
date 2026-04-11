trait Foo {
    fn bro(T:! Sized) -> i32;
}
impl Foo for i32 {
    fn bro(T:! Sized, U:! Sized) -> i32 {
        5
    }
}
fn main() -> i32 {
    0
}
