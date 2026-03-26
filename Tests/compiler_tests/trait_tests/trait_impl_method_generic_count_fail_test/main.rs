trait Foo {
    fn bro<T>() -> i32;
}
impl Foo for i32 {
    fn bro<T, U>() -> i32 {
        5
    }
}
fn main() -> i32 {
    0
}
