trait Double {
    fn double(self: &Self) -> i32;
}
impl Double for i32 {
    fn double(self: &i32) -> i32 {
        *self + *self
    }
}
fn main() -> i32 {
    let x = 5;
    x.double()
}
