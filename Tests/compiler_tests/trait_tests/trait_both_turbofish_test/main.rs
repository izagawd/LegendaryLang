trait Foo {
    fn kk<T>() -> i32;
}
fn bruh<T: Foo, U>() -> i32 {
    <T as Foo>::kk::<U>() + T::kk::<U>()
}
impl Foo for i32 {
    fn kk<T>() -> i32 { 5 }
}
fn main() -> i32 {
    bruh::<i32, i32>()
}
