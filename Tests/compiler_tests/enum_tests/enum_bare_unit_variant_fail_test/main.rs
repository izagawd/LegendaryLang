enum Status { Ok, Err }

fn main() -> i32 {
    match Status.Ok {
        Ok => 1,
        _ => 0
    }
}
