trait Foo {
    fn kk<T>() -> i32;
}
fn bruh<T: Foo, U>() -> i32 {
    <T as Foo>::kk::<U>();
    T::kk::<U>()
}
fn main() -> i32 {
    4
}
