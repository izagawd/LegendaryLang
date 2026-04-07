enum Color { Red, Green, Blue }
use crate.Color.Red;

fn main() -> i32 {
    match Color.Red {
        Red => 1,
        _ => 0
    }
}
