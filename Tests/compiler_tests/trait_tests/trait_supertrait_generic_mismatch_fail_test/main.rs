trait Bar<T> {
    fn bar_val() -> i32;
}
trait Foo: Bar<i32> {
    fn foo_val() -> i32;
}
fn needs_bar_bool<T: Bar<bool>>() -> i32 {
    T::bar_val()
}
fn try_it<T: Foo>() -> i32 {
    needs_bar_bool::<T>()
}
fn main() -> i32 {
    0
}
