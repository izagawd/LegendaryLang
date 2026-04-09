fn take_ref(x: &i32) -> i32 { *x }
fn main() -> i32 {
    take_ref(42)
}
