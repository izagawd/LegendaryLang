enum Dir { Up, Down }
use Dir.Up;

fn main() -> i32 {
    match Dir.Up {
        Up => 1,
        _ => 0
    }
}
