trait Foo {
    fn kk<T>() -> i32;
}
fn bruh<T: Foo, U>() -> i32 {
    T::kk::<U>()
}
impl Foo for i32 {
    fn kk<T>() -> i32 { 13 }
}
fn main() -> i32 {
    bruh::<i32, i32>()
}
