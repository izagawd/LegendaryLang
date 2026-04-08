trait Idk: Sized {
    fn do_thing(input: Self) -> i32;
}
impl Idk for i32 {
    fn do_thing(input: bool) -> i32 {
        5
    }
}
fn main() -> i32 {
    0
}
