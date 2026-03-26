trait Base {
    fn base_val() -> i32;
}
trait Child: Base {
    fn child_val() -> i32;
}
fn use_base<T: Base>() -> i32 {
    T::base_val()
}
fn use_child<T: Child>() -> i32 {
    use_base::<T>() + T::child_val()
}
impl Base for i32 {
    fn base_val() -> i32 { 10 }
}
impl Child for i32 {
    fn child_val() -> i32 { 20 }
}
fn main() -> i32 {
    use_child::<i32>()
}
