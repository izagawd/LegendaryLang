trait Foo{
    fn kk<T>();
}
fn bruh<T: Foo, U>(){
    <T as Foo>::kk::<U>();
    T::kk::<U>();
}
fn main() -> i32{
    4
}
