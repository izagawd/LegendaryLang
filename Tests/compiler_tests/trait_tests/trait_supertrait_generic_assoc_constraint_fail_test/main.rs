trait Producer {
    let Output :! type;
}

trait WantsString: Producer(Output = bool) {}

impl Producer for i32 {
    let Output :! type = i32;
}

impl WantsString for i32 {}

fn main() -> i32 {
    5
}
