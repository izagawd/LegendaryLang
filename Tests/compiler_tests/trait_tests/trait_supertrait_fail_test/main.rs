trait Base {
    fn base_val() -> i32;
}
trait Sub: Base {
    fn sub_val() -> i32;
}
fn needs_sub(T:! Sized +Sub) -> i32 {
    T.sub_val()
}
fn only_base(T:! Sized +Base) -> i32 {
    needs_sub(T)
}
fn main() -> i32 {
    0
}
