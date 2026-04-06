enum Choice {
    Yes(i32),
    No
}
fn extract(c: Choice) -> i32 {
    match c {
        Choice.Yes(v) => v,
        Choice.No => 0
    }
}
fn main() -> i32 {
    extract(Choice.Yes(15))
}
