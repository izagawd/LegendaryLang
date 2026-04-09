enum Dir {
    Up,
    Down
}
fn main() -> i32 {
    let d = Dir.Up;
    match d {
        Dir.Up => 42,
        Dir.Down => 0
    }
}
