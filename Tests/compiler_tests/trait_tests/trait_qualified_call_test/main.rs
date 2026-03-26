trait Default {
    fn default() -> Self;
}

impl Default for i32 {
    fn default() -> i32 {
        42
    }
}

fn main() -> i32 {
    let a : i32 = <i32 as Default>::default();
    a
}
