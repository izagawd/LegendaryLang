trait Base {
    fn base_val() -> i32;
}
trait Sub: Base {
    fn sub_val() -> i32;
}
fn needs_base<T: Base>() -> i32 {
    T::base_val()
}
fn needs_sub<T: Sub>() -> i32 {
    needs_base::<T>() + T::sub_val()
}
impl Base for i32 {
    fn base_val() -> i32 { 10 }
}
impl Sub for i32 {
    fn sub_val() -> i32 { 5 }
}
fn main() -> i32 {
    needs_sub::<i32>()
}
